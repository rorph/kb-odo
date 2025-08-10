#!/usr/bin/env python3
import sqlite3

db_path = "debug/odometer.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("Testing fixed queries against actual database:")
print("=" * 50)

# Test the exact queries used in C# code
queries = {
    "Today": """
        SELECT app_name, total_seconds
        FROM app_usage_daily
        ORDER BY total_seconds DESC
    """,
    "Weekly": """
        SELECT app_name, total_seconds
        FROM app_usage_weekly
        ORDER BY total_seconds DESC
    """,
    "Monthly": """
        SELECT app_name, total_seconds
        FROM app_usage_monthly
        ORDER BY total_seconds DESC
    """,
    "Lifetime": """
        SELECT app_name, total_seconds
        FROM app_usage_lifetime
        ORDER BY total_seconds DESC
    """
}

for time_range, query in queries.items():
    print(f"\n{time_range}:")
    try:
        cursor.execute(query)
        results = cursor.fetchall()
        print(f"  Found {len(results)} apps")
        for i, (app_name, seconds) in enumerate(results[:3]):
            print(f"  {i+1}. {app_name}: {seconds} seconds ({seconds/60:.1f} minutes)")
            # Check data types
            print(f"     Type of seconds: {type(seconds).__name__}")
    except Exception as e:
        print(f"  ERROR: {e}")

conn.close()