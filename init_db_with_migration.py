#!/usr/bin/env python3
import sqlite3
from datetime import datetime

def init_database(db_path):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print(f"Initializing database: {db_path}")
    
    # Enable WAL mode
    cursor.execute("PRAGMA journal_mode=WAL")
    
    # Create schema_version table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS schema_version (
            version INTEGER PRIMARY KEY,
            applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    # Create basic tables
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS daily_stats (
            date TEXT PRIMARY KEY,
            key_count INTEGER DEFAULT 0,
            mouse_distance REAL DEFAULT 0,
            left_clicks INTEGER DEFAULT 0,
            right_clicks INTEGER DEFAULT 0,
            middle_clicks INTEGER DEFAULT 0,
            scroll_distance REAL DEFAULT 0
        )
    """)
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS hourly_stats (
            date TEXT,
            hour INTEGER,
            key_count INTEGER DEFAULT 0,
            mouse_distance REAL DEFAULT 0,
            left_clicks INTEGER DEFAULT 0,
            right_clicks INTEGER DEFAULT 0,
            middle_clicks INTEGER DEFAULT 0,
            scroll_distance REAL DEFAULT 0,
            PRIMARY KEY (date, hour)
        )
    """)
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS key_stats (
            date TEXT,
            hour INTEGER,
            key_code TEXT,
            count INTEGER DEFAULT 0,
            PRIMARY KEY (date, hour, key_code)
        )
    """)
    
    # Create app_usage_stats table (Migration 4)
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
    
    # Create views for app usage
    cursor.execute("""
        CREATE VIEW IF NOT EXISTS app_usage_daily AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        WHERE date = date('now', 'localtime')
        GROUP BY app_name
    """)
    
    cursor.execute("""
        CREATE VIEW IF NOT EXISTS app_usage_weekly AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        WHERE date >= date('now', '-7 days', 'localtime')
        GROUP BY app_name
    """)
    
    cursor.execute("""
        CREATE VIEW IF NOT EXISTS app_usage_monthly AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        WHERE date >= date('now', '-30 days', 'localtime')
        GROUP BY app_name
    """)
    
    cursor.execute("""
        CREATE VIEW IF NOT EXISTS app_usage_lifetime AS
        SELECT 
            app_name,
            SUM(seconds_used) as total_seconds
        FROM app_usage_stats
        GROUP BY app_name
    """)
    
    # Set schema version to 4
    cursor.execute("INSERT OR REPLACE INTO schema_version (version) VALUES (4)")
    
    # Add some test data for today
    today = datetime.now().strftime("%Y-%m-%d")
    hour = datetime.now().hour
    
    test_apps = [
        ("Visual Studio Code", 3600),
        ("Chrome", 2400),
        ("Terminal", 1800),
        ("Slack", 900),
        ("Notepad", 600)
    ]
    
    for app_name, seconds in test_apps:
        cursor.execute("""
            INSERT OR REPLACE INTO app_usage_stats (date, hour, app_name, seconds_used)
            VALUES (?, ?, ?, ?)
        """, (today, hour, app_name, seconds))
    
    conn.commit()
    
    # Verify
    cursor.execute("SELECT COUNT(*) FROM app_usage_stats")
    count = cursor.fetchone()[0]
    print(f"Created {count} app usage records")
    
    cursor.execute("SELECT * FROM app_usage_daily")
    daily = cursor.fetchall()
    print("\nDaily app usage view:")
    for row in daily:
        print(f"  {row[0]}: {row[1]} seconds")
    
    conn.close()
    print("\nDatabase initialized successfully!")

if __name__ == "__main__":
    init_database("test_app_db.db")