#!/usr/bin/env bash
set -euo pipefail

data_dir="${FIREBASE_EMULATOR_DATA_DIR:-/srv/firebase/.data}"
project_id="${FIREBASE_PROJECT_ID:-tasklet-app}"

mkdir -p "$data_dir"

firebase emulators:start \
  --only auth \
  --project "$project_id" \
  --import "$data_dir" \
  --export-on-exit "$data_dir" &
firebase_pid="$!"

export_loop() {
  while true; do
    sleep "${FIREBASE_EMULATOR_EXPORT_INTERVAL_SECONDS:-5}"
    firebase emulators:export --only auth --project "$project_id" --force "$data_dir" || true
  done
}

export_loop &
export_loop_pid="$!"

shutdown() {
  trap - SIGINT SIGTERM

  # Aspire resource stops do not reliably reach Firebase as Ctrl-C, so export
  # through the running emulator hub before letting the child process exit.
  firebase emulators:export --only auth --project "$project_id" --force "$data_dir" || true
  kill "$export_loop_pid" 2>/dev/null || true
  kill -INT "$firebase_pid" 2>/dev/null || true
  wait "$firebase_pid" || true
}

trap shutdown SIGINT SIGTERM

wait "$firebase_pid"
