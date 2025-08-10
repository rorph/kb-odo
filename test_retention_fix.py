# Test to verify the retention fix logic
from datetime import datetime, timedelta

def test_cutoff_calculation(retention_days):
    """Simulate the old buggy behavior vs new fixed behavior"""
    today = datetime(2025, 8, 9)  # From the debug logs
    
    if retention_days <= 0:
        print(f"Retention={retention_days}: SKIP cleanup (keep forever) ✅")
        return None
    else:
        cutoff = today - timedelta(days=retention_days)
        print(f"Retention={retention_days}: Delete data before {cutoff.strftime('%Y-%m-%d')}")
        return cutoff

# Test cases
print("=== Testing Retention Logic ===\n")
test_cutoff_calculation(0)    # Should skip
test_cutoff_calculation(-1)   # Should skip  
test_cutoff_calculation(1)    # Should delete before 2025-08-08
test_cutoff_calculation(7)    # Should delete before 2025-08-02
test_cutoff_calculation(30)   # Should delete before 2025-07-10
test_cutoff_calculation(90)   # Should delete before 2025-05-11

print("\n=== Data from debug folder ===")
print("Daily stats: only 2025-08-09 (today)")
print("Key stats: 2025-08-04 to 2025-08-09")
print("\nWith retention=0, all this data should be KEPT ✅")
