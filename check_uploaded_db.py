#!/usr/bin/env python3
import sqlite3

db_path = "debug/odometer.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("=== DATABASE STRUCTURE ===")
cursor.execute("SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') ORDER BY type, name")
items = cursor.fetchall()
for name, item_type in items:
    print(f"{item_type.upper()}: {name}")

# Check schema version
try:
    cursor.execute("SELECT version FROM schema_version")
    version = cursor.fetchone()
    print(f"\nSchema version: {version[0] if version else 'None'}")
except:
    print("\nNo schema_version table")

# Check if app_usage_stats table exists
cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='app_usage_stats'")
if cursor.fetchone():
    print("\n✓ app_usage_stats table EXISTS")
    cursor.execute("SELECT COUNT(*) FROM app_usage_stats")
    count = cursor.fetchone()[0]
    print(f"  Records: {count}")
    
    # Show sample data
    cursor.execute("SELECT * FROM app_usage_stats ORDER BY date DESC, hour DESC LIMIT 10")
    rows = cursor.fetchall()
    if rows:
        print("\n  Sample data:")
        for row in rows:
            print(f"    {row}")
else:
    print("\n✗ app_usage_stats table DOES NOT EXIST")

# Check views
for view in ['app_usage_daily', 'app_usage_weekly', 'app_usage_monthly', 'app_usage_lifetime']:
    cursor.execute(f"SELECT name FROM sqlite_master WHERE type='view' AND name='{view}'")
    if cursor.fetchone():
        cursor.execute(f"SELECT COUNT(*) FROM {view}")
        count = cursor.fetchone()[0]
        print(f"\n✓ {view} view exists with {count} records")
        
        if count > 0:
            cursor.execute(f"SELECT * FROM {view} LIMIT 3")
            rows = cursor.fetchall()
            for row in rows:
                print(f"    {row}")
    else:
        print(f"\n✗ {view} view DOES NOT EXIST")

conn.close()