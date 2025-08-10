#!/usr/bin/env python3
import sqlite3

conn = sqlite3.connect("odometer.db")
cursor = conn.cursor()

print("ALL TABLES AND VIEWS:")
print("=" * 50)

# Check all tables
cursor.execute("SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') ORDER BY type, name")
items = cursor.fetchall()
for name, item_type in items:
    print(f"{item_type.upper()}: {name}")
    
# Check schema version
cursor.execute("SELECT version FROM schema_version")
version = cursor.fetchone()
print(f"\nSchema version: {version[0] if version else 'None'}")

# Try to see if application_usage table exists
print("\n--- CHECKING FOR APP USAGE RELATED TABLES ---")
cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%app%'")
app_tables = cursor.fetchall()
print(f"App-related tables: {app_tables}")

conn.close()