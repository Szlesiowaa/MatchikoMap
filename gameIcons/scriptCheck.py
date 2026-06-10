import os

BASE_DIR = "./games"
ALLOWED_NAMES = {"icon", "poster", "grid"}

def find_invalid_files():
    for game_folder in os.listdir(BASE_DIR):
        game_path = os.path.join(BASE_DIR, game_folder)

        if not os.path.isdir(game_path):
            continue

        for file in os.listdir(game_path):
            file_path = os.path.join(game_path, file)

            if not os.path.isfile(file_path):
                continue

            name, ext = os.path.splitext(file)

            if name.lower() not in ALLOWED_NAMES:
                print(f"NIEPOPRAWNY: {file_path}")

if __name__ == "__main__":
    find_invalid_files()