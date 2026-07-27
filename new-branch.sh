# script for creating a new branch from main and opening it in VS Code using git worktree
#!/bin/bash

# Check if a parameter is provided
if [ -z "$1" ]; then
  echo "Usage: ./new-branch.sh <name-of-branch>"
  exit 1
fi

NAME=$1

# Navigate to the repository root
cd /c/repos/sheetmusic-api

# Check for pending changes
if [ -n "$(git status --porcelain)" ]; then
  echo "There are pending changes in the repository. Commit or stash them before proceeding."
  exit 1
fi

git checkout main
git pull
git worktree add -b "$NAME" "../sapi-$NAME"
cd "../sapi-$NAME"
code .
