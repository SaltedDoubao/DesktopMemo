# Debugging & Logging

## Log Location

Default path: `.memodata/.logs/`

## Common Troubleshooting Approaches

- Startup failure: First check the latest log file for exception stack traces
- Data anomalies: Focus on "Migration / Repository / Database" related logs
- UI behavior issues: Combine `Debug.WriteLine` output with log records for debugging

## Information to Provide When Reporting Issues

- Version number (Release name or application About information)
- Steps to reproduce
- Key log fragments (after redacting sensitive information)
- Whether you are an upgrade user, whether you've migrated old data

---

**Last Updated**: 2025-12-26
