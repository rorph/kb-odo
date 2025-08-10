#!/usr/bin/env python3
import sqlite3

db_path = "debug/odometer.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("=== VIEW DEFINITIONS ===\n")

# Check view definitions
for view in ['app_usage_daily', 'app_usage_weekly', 'app_usage_monthly', 'app_usage_lifetime']:
    cursor.execute(f"SELECT sql FROM sqlite_master WHERE type='view' AND name='{view}'")
    result = cursor.fetchone()
    if result:
        print(f"{view}:")
        print(result[0])
        print()

conn.close()