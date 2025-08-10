#!/usr/bin/env python3
import sqlite3

conn = sqlite3.connect("test_app_db.db")
cursor = conn.cursor()

print("Testing app usage queries:")
print("=" * 50)

# Test the query used in GetTodayAppUsageAsync - IT'S MISSING THE WHERE CLAUSE!
print("\n1. INCORRECT Query (missing WHERE clause):")
cursor.execute("""
    SELECT app_name, total_seconds as seconds_used
    FROM app_usage_daily
    ORDER BY total_seconds DESC
""")
results = cursor.fetchall()
print(f"   Found {len(results)} records")
for row in results[:5]:
    print(f"   {row[0]}: {row[1]} seconds")

# The view already filters for today, so the query works!
print("\n2. View definition for app_usage_daily:")
cursor.execute("SELECT sql FROM sqlite_master WHERE name='app_usage_daily'")
view_def = cursor.fetchone()
print(view_def[0])

print("\n3. Testing all views:")
for view in ['app_usage_daily', 'app_usage_weekly', 'app_usage_monthly', 'app_usage_lifetime']:
    cursor.execute(f"SELECT COUNT(*) FROM {view}")
    count = cursor.fetchone()[0]
    print(f"   {view}: {count} records")

conn.close()