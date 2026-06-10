import requests
import constants
import os
import shutil
import sys
import re
from pathlib import Path

def sanitize_filename(name):
    # usuwa niedozwolone znaki w Windows
    return re.sub(r'[<>:"/\\|?*]', '', name)

BASE_PATH = Path(__file__).parent / "games"

def create_folder(base_path, game_name):
    safe_name = sanitize_filename(game_name)
    folder_path = os.path.join(base_path, safe_name)
    if os.path.exists(folder_path):
        shutil.rmtree(folder_path)
    os.makedirs(folder_path)
    return folder_path

def check_query(input_name):
    if not input_name:
        return None, None

    id_url = "https://www.steamgriddb.com/api/v2/games/id/" + input_name
    name_search_url = "https://www.steamgriddb.com/api/v2/search/autocomplete/" + input_name

    headers = {"Authorization": f"Bearer {constants.api_key}"}

    # próbuj jako ID
    id_response = requests.get(id_url, headers=headers).json()
    if id_response['success']:
        data = id_response['data']
        return str(data['id']), data['name']

    # próbuj jako nazwa
    name_response = requests.get(name_search_url, headers=headers).json()
    if name_response['success'] and len(name_response['data']) > 0:
        first = name_response['data'][0]
        return str(first['id']), first['name']

    return None, None

def download_image(url, path, filename):
    response = requests.get(url, stream=True)
    if response.status_code == 200:
        with open(os.path.join(path, filename), 'wb') as f:
            for chunk in response.iter_content(1024):
                f.write(chunk)
        return True
    return False

def handle_images(game_id, path):
    headers = {"Authorization": f"Bearer {constants.api_key}"}

    endpoints = {
        "grid": ("https://www.steamgriddb.com/api/v2/grids/game/" + game_id, {"dimensions": "920x430,460x215"}),
        "poster": ("https://www.steamgriddb.com/api/v2/grids/game/" + game_id, {"dimensions": "600x900"}),
        "hero": ("https://www.steamgriddb.com/api/v2/heroes/game/" + game_id, {}),
        "logo": ("https://www.steamgriddb.com/api/v2/logos/game/" + game_id, {}),
        "icon": ("https://www.steamgriddb.com/api/v2/icons/game/" + game_id, {}),
    }

    results = {}

    for category, (url, params) in endpoints.items():
        response = requests.get(url, params=params, headers=headers)

        if response.status_code != 200:
            print(f"{category}: request failed")
            results[category] = False
            continue

        data = response.json()
        if not data["success"] or len(data["data"]) == 0:
            print(f"{category}: brak obrazów")
            results[category] = False
            continue

        # bierz pierwszy poprawny obraz
        for img in data["data"]:
            if not img["url"].endswith("?"):
                ext = ".png" if "png" in img["mime"] else ".jpg"
                filename = f"{game_id}_{category}{ext}"
                success = download_image(img["url"], path, filename)
                results[category] = success
                break
        else:
            results[category] = False

    return results


def main():
    not_found_file = open("nieznalezione.txt", "w", encoding="utf-8")

    with open("lista_gier.txt", "r", encoding="utf-8") as f:
        games = [line.strip() for line in f if line.strip()]

    for game in games:
        print(f"\nSzukam: {game}")

        game_id, matched_name = check_query(game)

        if not game_id:
            print("Nie znaleziono")
            not_found_file.write(game + "\n")
            continue

        print(f"Znaleziono: {matched_name}")

        # PAUZA NA WERYFIKACJĘ
        decision = input("Enter = OK, s = skip: ").lower()
        if decision == "s":
            print("Pominięto")
            continue

        game_path = create_folder(BASE_PATH, matched_name)

        results = handle_images(game_id, game_path)

        print("Wyniki:")
        for k, v in results.items():
            print(f"{k}: {'OK' if v else 'FAIL'}")

    not_found_file.close()
    print("\nGotowe")


if __name__ == "__main__":
    main()
