import os
import json
import urllib.parse

BASE_DIR = "./games"
GAMES_FILE = "lista_gier.txt"
OUTPUT_FILE = "games.json"
AZURE_LINK = "https://matchikomapblobstorage.blob.core.windows.net"

def find_file(folder_path, target_name):
    for file in os.listdir(folder_path):
        name, ext = os.path.splitext(file)
        if name.lower() == target_name:
            return os.path.join(folder_path, file).replace("\\", "/")
    return None

def main():
    # wczytaj listę gier
    with open(GAMES_FILE, "r", encoding="utf-8") as f:
        game_names = [line.strip() for line in f if line.strip()]

    # pobierz katalogi
    folders = [f for f in os.listdir(BASE_DIR) if os.path.isdir(os.path.join(BASE_DIR, f))]

    # sortowanie case-insensitive
    game_names.sort(key=lambda x: x.lower())
    folders.sort(key=lambda x: x.lower())

    if len(game_names) != len(folders):
        print("⚠️ UWAGA: liczba gier i katalogów się nie zgadza")

    result = []

    for i in range(min(len(game_names), len(folders))):
        game_name = game_names[i]
        folder = folders[i]
        folder_path = os.path.join(BASE_DIR, folder)

        icon = find_file(folder_path, "icon")
        grid = find_file(folder_path, "grid")
        poster = find_file(folder_path, "poster")

        def clean_path(p):
            if not p: return None
            # zamienia "./games/..." na "games/..."
            cleaned = p.lstrip("./")
            return f"game-art/{cleaned}"
        
        result.append({
            "name": game_name,
            "iconPath": f"{AZURE_LINK}/{urllib.parse.quote(clean_path(icon))}" if icon else None,
            "gridPath": f"{AZURE_LINK}/{urllib.parse.quote(clean_path(grid))}" if grid else None,
            "posterPath": f"{AZURE_LINK}/{urllib.parse.quote(clean_path(poster))}" if poster else None
        })


    # zapis JSON
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=4, ensure_ascii=False)

    print(f"Zapisano do {OUTPUT_FILE}")

if __name__ == "__main__":
    main()
