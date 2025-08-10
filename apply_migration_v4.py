#!/usr/bin/env python3
import sqlite3
import sys
from datetime import datetime

def apply_migration_v4(db_path):
    print(f"Applying Migration v4 to: {db_path}")
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # Check current version
    try:
        cursor.execute("SELECT MAX(version) FROM schema_version")
        current_version = cursor.fetchone()[0] or 0
        print(f"Current schema version: {current_version}")
    except:
        current_version = 0
        print("No schema_version table found, treating as version 0")
    
    if current_version >= 4:
        print("Database already at version 4 or higher")
        return
    
    print("Applying migration to version 4...")
    
    # Create app_usage_stats table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS app_usage_stats (
            date TEXT,
            hour INTEGER,
            app_name TEXT,
            seconds_used INTEGER DEFAULT 0,
            PRIMARY KEY (date, hour, app_name)
        )
    """)
    
    # Create indexes
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_app_usage_date ON app_usage_stats(date)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_app_usage_app ON app_usage_stats(app_name)")
    cursor.execute("CREATE INDEX IF NOT EXISTS idx_app_usage_date_app ON app_usage_stats(date, app_name)")
    
    # Drop existing views if they exist
    cursor.execute("DROP VIEW IF EXISTS app_usage_daily")
    cursor.execute("DROP VIEW IF EXISTS app_usage_weekly")
    cursor.execute("DROP VIEW IF EXISTS app_usage_monthly")
    cursor.execute("DROP VIEW IF EXISTS app_usage_lifetime")
    
    # Create views
    cursor.execute("""
        CREATE VIEW app_usage_daily AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        WHERE date = date('now', 'localtime')
        GROUP BY app_name
    """)
    
    cursor.execute("""
        CREATE VIEW app_usage_weekly AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        WHERE date >= date('now', '-7 days', 'localtime')
        GROUP BY app_name
    """)
    
    cursor.execute("""
        CREATE VIEW app_usage_monthly AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        WHERE date >= date('now', '-30 days', 'localtime')
        GROUP BY app_name
    """)
    
    cursor.execute("""
        CREATE VIEW app_usage_lifetime AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        GROUP BY app_name
    """)
    
    # Update schema version
    cursor.execute("INSERT OR REPLACE INTO schema_version (version, applied_at) VALUES (4, datetime('now'))")
    
    # Add some sample data for testing
    today = datetime.now().strftime("%Y-%m-%d")
    hour = datetime.now().hour
    
    sample_apps = [
        ("Visual Studio Code", 1800),
        ("Terminal", 900),
        ("Chrome", 600)
    ]
    
    for app_name, seconds in sample_apps:
        cursor.execute("""
            INSERT OR IGNORE INTO app_usage_stats (date, hour, app_name, seconds_used)
            VALUES (?, ?, ?, ?)
        """, (today, hour, app_name, seconds))
    
    conn.commit()
    
    # Verify
    cursor.execute("SELECT COUNT(*) FROM app_usage_stats")
    count = cursor.fetchone()[0]
    print(f"App usage stats table has {count} records")
    
    cursor.execute("SELECT * FROM app_usage_daily LIMIT 5")
    daily = cursor.fetchall()
    if daily:
        print("\nSample daily app usage:")
        for row in daily:
            print(f"  {row[0]}: {row[1]} seconds")
    
    cursor.execute("SELECT version FROM schema_version")
    version = cursor.fetchone()[0]
    print(f"\nDatabase now at version: {version}")
    
    conn.close()
    print("Migration completed successfully!")

if __name__ == "__main__":
    db_path = sys.argv[1] if len(sys.argv) > 1 else "debug/odometer.db"
    apply_migration_v4(db_path)