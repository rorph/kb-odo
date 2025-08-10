#!/usr/bin/env python3
import sqlite3
import json
from datetime import datetime

def analyze_database(db_path):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("DATABASE ANALYSIS REPORT")
    print("=" * 50)
    
    # Check tables
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = cursor.fetchall()
    print(f"\nTables found: {[t[0] for t in tables]}")
    
    # Check schema version
    try:
        cursor.execute("SELECT version FROM schema_version")
        version = cursor.fetchone()
        print(f"Schema version: {version[0] if version else 'None'}")
    except:
        print("Schema version table not found or empty")
    
    # Check daily_stats
    print("\n--- DAILY STATS ---")
    cursor.execute("SELECT * FROM daily_stats ORDER BY date")
    daily_stats = cursor.fetchall()
    print(f"Total daily records: {len(daily_stats)}")
    for row in daily_stats[:10]:  # Show first 10
        print(f"  Date: {row[0]}, Keys: {row[1]}, Mouse: {row[2]:.2f}m, Clicks: L={row[3]}, R={row[4]}, M={row[5]}, Scroll: {row[6]:.2f}m")
    
    # Check hourly_stats
    print("\n--- HOURLY STATS ---")
    cursor.execute("SELECT date, COUNT(*) FROM hourly_stats GROUP BY date ORDER BY date")
    hourly_groups = cursor.fetchall()
    print(f"Days with hourly data: {len(hourly_groups)}")
    for date, count in hourly_groups:
        print(f"  {date}: {count} hours")
    
    # Check for data anomalies
    print("\n--- DATA ANOMALIES ---")
    
    # Check for duplicate dates in daily_stats
    cursor.execute("SELECT date, COUNT(*) FROM daily_stats GROUP BY date HAVING COUNT(*) > 1")
    duplicates = cursor.fetchall()
    if duplicates:
        print(f"DUPLICATE DATES FOUND: {duplicates}")
    else:
        print("No duplicate dates in daily_stats")
    
    # Check for future dates
    today = datetime.now().strftime("%Y-%m-%d")
    cursor.execute("SELECT date FROM daily_stats WHERE date > ?", (today,))
    future_dates = cursor.fetchall()
    if future_dates:
        print(f"FUTURE DATES FOUND: {future_dates}")
    else:
        print("No future dates found")
    
    # Check for data gaps
    cursor.execute("SELECT MIN(date), MAX(date) FROM daily_stats")
    date_range = cursor.fetchone()
    if date_range[0] and date_range[1]:
        print(f"Date range: {date_range[0]} to {date_range[1]}")
    
    # Check key_stats table
    print("\n--- KEY STATS ---")
    try:
        cursor.execute("SELECT COUNT(*) FROM key_stats")
        key_count = cursor.fetchone()[0]
        print(f"Total key stat records: {key_count}")
        
        cursor.execute("SELECT date, COUNT(DISTINCT key_code) FROM key_stats GROUP BY date ORDER BY date")
        key_days = cursor.fetchall()
        for date, unique_keys in key_days[:5]:
            print(f"  {date}: {unique_keys} unique keys")
    except:
        print("key_stats table not found or error reading it")
    
    # Check SQLite journal mode
    cursor.execute("PRAGMA journal_mode")
    journal_mode = cursor.fetchone()
    print(f"\n--- DATABASE SETTINGS ---")
    print(f"Journal mode: {journal_mode[0] if journal_mode else 'Unknown'}")
    
    # Check for WAL mode
    cursor.execute("PRAGMA wal_checkpoint")
    
    # Check database integrity
    cursor.execute("PRAGMA integrity_check")
    integrity = cursor.fetchone()
    print(f"Integrity check: {integrity[0] if integrity else 'Unknown'}")
    
    conn.close()

if __name__ == "__main__":
    analyze_database("odometer_analysis.db")