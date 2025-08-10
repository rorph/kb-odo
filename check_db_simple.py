#!/usr/bin/env python3
import sqlite3

conn = sqlite3.connect("odometer.db")
cursor = conn.cursor()

# Get all tables
cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
tables = cursor.fetchall()
print("Tables:", [t[0] for t in tables])

# Get all views  
cursor.execute("SELECT name FROM sqlite_master WHERE type='view'")
views = cursor.fetchall()
print("Views:", [v[0] for v in views])

conn.close()