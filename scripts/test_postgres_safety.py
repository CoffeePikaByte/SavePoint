import importlib.util
import pathlib
import unittest
from unittest import mock
import subprocess
import signal

spec = importlib.util.spec_from_file_location("runner", pathlib.Path(__file__).with_name("test_postgres.py"))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class RunnerSafetyTests(unittest.TestCase):
    def test_requires_enable(self):
        with self.assertRaises(RuntimeError):
            runner.validate_environment({})

    def test_rejects_caller_supplied_connection(self):
        with self.assertRaises(RuntimeError):
            runner.validate_environment({"SAVEPOINT_POSTGRES_TESTS": "1", "SAVEPOINT_TEST_CONNECTION": "anything"})

    def test_rejects_remote_docker(self):
        with self.assertRaises(RuntimeError):
            runner.validate_environment({"SAVEPOINT_POSTGRES_TESTS": "1", "DOCKER_HOST": "tcp://remote:2375"})

    def test_accepts_explicit_local_execution(self):
        runner.validate_environment({"SAVEPOINT_POSTGRES_TESTS": "1"})

    def test_resource_identity_requires_both_labels(self):
        labels = {"com.docker.compose.project": "spit_project", "io.savepoint.integration.run": "run"}
        runner.verify_labels(labels, "spit_project", "run")
        for wrong in ({}, {**labels, "io.savepoint.integration.run": "other"},
                      {**labels, "com.docker.compose.project": "development"}):
            with self.assertRaises(RuntimeError):
                runner.verify_labels(wrong, "spit_project", "run")

    def test_test_timeout_terminates_only_the_launched_process_group(self):
        process = mock.Mock(pid=12345)
        process.poll.return_value = None
        process.wait.side_effect = [subprocess.TimeoutExpired("dotnet", 300), 0]
        with mock.patch.object(runner.subprocess, "Popen", return_value=process) as launch, \
                mock.patch.object(runner, "group_alive", return_value=True), \
                mock.patch.object(runner, "wait_group_exit", return_value=True), \
                mock.patch.object(runner.os, "killpg") as kill:
            with self.assertRaises(RuntimeError):
                runner.run_tests(["dotnet", "test"], {}, pathlib.Path("."))
            self.assertTrue(launch.call_args.kwargs["start_new_session"])
            kill.assert_called_once_with(12345, signal.SIGTERM)

    def test_stuck_test_process_group_is_force_killed_after_grace_period(self):
        process = mock.Mock(pid=12345)
        process.poll.return_value = None
        process.wait.side_effect = [subprocess.TimeoutExpired("dotnet", 300), subprocess.TimeoutExpired("dotnet", 10), 0]
        with mock.patch.object(runner.subprocess, "Popen", return_value=process), \
                mock.patch.object(runner, "group_alive", return_value=True), \
                mock.patch.object(runner, "wait_group_exit", side_effect=[False, True]), \
                mock.patch.object(runner.os, "killpg") as kill:
            with self.assertRaises(RuntimeError):
                runner.run_tests(["dotnet", "test"], {}, pathlib.Path("."))
            self.assertEqual([mock.call(12345, signal.SIGTERM), mock.call(12345, signal.SIGKILL)], kill.call_args_list)

    def test_exited_leader_does_not_hide_a_surviving_testhost(self):
        process = mock.Mock(pid=12345)
        process.poll.return_value = 0
        process.wait.side_effect = [subprocess.TimeoutExpired("dotnet", 300), 0, 0]
        with mock.patch.object(runner.subprocess, "Popen", return_value=process), \
                mock.patch.object(runner, "group_alive", return_value=True, create=True), \
                mock.patch.object(runner, "wait_group_exit", side_effect=[False, True], create=True), \
                mock.patch.object(runner.os, "killpg") as kill:
            with self.assertRaises(RuntimeError):
                runner.run_tests(["dotnet", "test"], {}, pathlib.Path("."))
            kill.assert_any_call(12345, signal.SIGTERM)
            kill.assert_any_call(12345, signal.SIGKILL)


if __name__ == "__main__":
    unittest.main()
