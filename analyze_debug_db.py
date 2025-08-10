import sqlite3
import json
from datetime import datetime

def analyze_database(db_path):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("=== DATABASE ANALYSIS ===\n")
    
    # Get all tables
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
    tables = cursor.fetchall()
    print(f"Tables: {[t[0] for t in tables]}\n")
    
    # Check schema version
    try:
        cursor.execute("SELECT MAX(version) FROM schema_version")
        version = cursor.fetchone()[0]
        print(f"Schema version: {version}\n")
    except:
        print("No schema_version table\n")
    
    # Analyze daily_stats
    print("=== DAILY_STATS ===")
    cursor.execute("SELECT date, key_count, mouse_distance, left_clicks FROM daily_stats ORDER BY date")
    daily_stats = cursor.fetchall()
    for row in daily_stats:
        print(f"Date: {row[0]}, Keys: {row[1]}, Mouse: {row[2]:.2f}m, Clicks: {row[3]}")
    print(f"Total days: {len(daily_stats)}\n")
    
    # Analyze hourly_stats
    print("=== HOURLY_STATS ===")
    cursor.execute("SELECT date, COUNT(*), SUM(key_count) FROM hourly_stats GROUP BY date ORDER BY date")
    hourly_summary = cursor.fetchall()
    for row in hourly_summary:
        print(f"Date: {row[0]}, Hours: {row[1]}, Total Keys: {row[2]}")
    
    # Check for key_stats
    print("\n=== KEY_STATS ===")
    cursor.execute("SELECT date, COUNT(DISTINCT key_code), SUM(count) FROM key_stats GROUP BY date ORDER BY date")
    key_summary = cursor.fetchall()
    for row in key_summary:
        print(f"Date: {row[0]}, Unique Keys: {row[1]}, Total Presses: {row[2]}")
    
    # Check views
    print("\n=== VIEWS ===")
    cursor.execute("SELECT name FROM sqlite_master WHERE type='view';")
    views = cursor.fetchall()
    print(f"Views: {[v[0] for v in views]}")
    
    conn.close()

analyze_database('debug/odometer.db')
