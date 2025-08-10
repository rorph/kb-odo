#!/usr/bin/env python3
import sqlite3
from datetime import datetime

def check_app_usage(db_path):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("APP USAGE DATA ANALYSIS")
    print("=" * 50)
    
    # Check app_usage table
    print("\n--- APP_USAGE TABLE ---")
    cursor.execute("SELECT * FROM app_usage ORDER BY date DESC, app_name LIMIT 20")
    app_usage = cursor.fetchall()
    print(f"Total app_usage records: {len(app_usage)}")
    for row in app_usage:
        print(f"  Date: {row[0]}, App: {row[1]}, Seconds: {row[2]}")
    
    # Check views
    print("\n--- APP USAGE VIEWS ---")
    
    # Check view definitions
    cursor.execute("SELECT name, sql FROM sqlite_master WHERE type='view' AND name LIKE 'app_usage_%'")
    views = cursor.fetchall()
    for view_name, view_sql in views:
        print(f"\n{view_name}:")
        print(view_sql)
        
        # Query the view
        cursor.execute(f"SELECT * FROM {view_name} LIMIT 5")
        view_data = cursor.fetchall()
        print(f"Records in {view_name}: {len(view_data)}")
        for row in view_data:
            print(f"  {row}")
    
    conn.close()

if __name__ == "__main__":
    check_app_usage("odometer.db")