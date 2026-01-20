import random
import subprocess
import os
import shutil
import tempfile
import time
import json
from datetime import datetime
from playwright.sync_api import sync_playwright

PROFILE_PATH = "/home/kikono/.mozilla/firefox/ucdycsy7.default-release-2"
CACHE_FILE = "./scripts/mix_cache.json"

def get_cached_mixes():
    """Returns the list of mixes if cached today, otherwise None."""
    if not os.path.exists(CACHE_FILE):
        return None
    
    try:
        with open(CACHE_FILE, 'r') as f:
            data = json.load(f)
            
        cached_date = data.get("date")
        today = datetime.now().strftime("%Y-%m-%d")
        
        if cached_date == today:
            print("Using cached mixes from today...")
            return data.get("urls", [])
    except Exception as e:
        print(f"Error reading cache: {e}")
    
    return None

def save_mixes_to_cache(urls):
    """Saves the list of URLs with today's date stamp."""
    data = {
        "date": datetime.now().strftime("%Y-%m-%d"),
        "urls": urls
    }
    with open(CACHE_FILE, 'w') as f:
        json.dump(data, f)
    print("Mixes saved to cache for today.")

def scrape_youtube_mixes():
    """The original Playwright logic to find mixes."""
    with tempfile.TemporaryDirectory() as temp_dir:
        clone_path = os.path.join(temp_dir, "profile_clone")
        shutil.copytree(PROFILE_PATH, clone_path, symlinks=True, dirs_exist_ok=True)
        
        lock_file = os.path.join(clone_path, ".parentlock")
        if os.path.exists(lock_file):
            os.remove(lock_file)

        with sync_playwright() as p:
            context = p.firefox.launch_persistent_context(user_data_dir=clone_path, headless=True)
            page = context.new_page()
            
            print("Loading YouTube Homepage to find new mixes...")
            page.goto("https://www.youtube.com", wait_until="networkidle")
            page.wait_for_timeout(2000)

            try:
                for _ in range(3):
                    page.mouse.wheel(0, 1000)
                    page.wait_for_timeout(1500)
                
                page.wait_for_selector('a[href*="list=RD"]', timeout=20000)
                
                mix_urls = page.evaluate('''() => {
                    return Array.from(document.querySelectorAll('a[href*="list=RD"]')).map(a => a.href).slice(0, 24);
                }''')
                
                return list(set(mix_urls)) if mix_urls else []
            
            except Exception as e:
                print(f"Scraping failed: {e}")
                return []
            finally:
                context.close()

def open_mix_and_play():
    mix_urls = get_cached_mixes()

    if not mix_urls:
        mix_urls = scrape_youtube_mixes()
        if mix_urls:
            save_mixes_to_cache(mix_urls)

    if mix_urls:
        selected_mix = random.choice(mix_urls)
        final_url = f"{selected_mix}&autoplay=1"
        
        print(f"Success! Opening: {final_url}")
        subprocess.run(["firefox", "--new-tab", final_url])

        print("Sending Play command (xdotool) in 8 seconds...")
        time.sleep(8)
        subprocess.run(["xdotool", "key", "k"])
    else:
        print("No Mixes found or available.")

if __name__ == "__main__":
    open_mix_and_play()