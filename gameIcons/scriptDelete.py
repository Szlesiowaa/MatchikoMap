import os

BASE_DIR = "./games"

def delete_heroes():
    for game_folder in os.listdir(BASE_DIR):
        game_path = os.path.join(BASE_DIR, game_folder)

        if not os.path.isdir(game_path):
            continue

        files = os.listdir(game_path)

        hero_files = [f for f in files if "hero" in f.lower()]

        if not hero_files:
            print(f"Brak hero w: {game_folder} → pomijam")
            continue

        for file in hero_files:
            file_path = os.path.join(game_path, file)

            if os.path.exists(file_path):
                os.remove(file_path)
                print(f"Usunięto: {file_path}")
            else:
                print(f"Nie znaleziono: {file_path}")

if __name__ == "__main__":
    delete_heroes()