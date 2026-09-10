# Agent Instructions

## Verification

- Do NOT write scratch probe programs, harnesses, or throwaway test projects
  (in `/tmp`, in the repo, or anywhere else) to verify changes.
- Do NOT add test frameworks, test projects, or test files to the repo.
- Verify by building (`dotnet build -c Debug`) and by reading the affected
  code paths. A clean build plus code review is sufficient verification.
- Do not run the server.
