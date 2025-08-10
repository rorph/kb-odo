#!/usr/bin/env python3
import sqlite3

for db_path in ["debug/odometer.db", "odometer_analysis.db"]:
    print(f"\n=== CHECKING {db_path} ===")
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Get all tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = cursor.fetchall()
        print("Tables:", [t[0] for t in tables])
        
        # Check for app_usage table
        if any('app' in t[0].lower() for t in tables):
            print("\nFound app-related tables!")
            cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%app%'")
            app_tables = cursor.fetchall()
            for table in app_tables:
                cursor.execute(f"SELECT COUNT(*) FROM {table[0]}")
                count = cursor.fetchone()[0]
                print(f"  {table[0]}: {count} records")
                
                # Show sample data
                if count > 0:
                    cursor.execute(f"SELECT * FROM {table[0]} LIMIT 3")
                    rows = cursor.fetchall()
                    for row in rows:
                        print(f"    {row}")
        
        conn.close()
    except Exception as e:
        print(f"Error: {e}")