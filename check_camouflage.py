import glob
import json
from PIL import Image
from google import genai
from google.genai import types
from pydantic import BaseModel, Field


class DetectedSlug(BaseModel):
    box_2d: list[int] = Field(description="検出したナメクジの座標 [ymin, xmin, ymax, xmax] (0〜1000の正規化座標)")
    body_color_guess: str = Field(description="見えた体の色（苔色、赤土色、炭色、灰白など）")
    visibility_score: int = Field(description="見つけやすさ（1: ほぼ完全に背景に溶け込んでいる 〜 10: 一目で輪郭や色が浮いて見える）")
    outline_noticeable: bool = Field(description="黒い輪郭線（トゥーンのアウトライン）が目立って位置バレの原因になっているか")
    reason: str = Field(description="なぜ見つけやすかったか／見つけにくかったかの理由（明度差、輪郭線、シルエットなど）")


class CamouflageReport(BaseModel):
    detected_count: int = Field(description="画面内で見つけられたナメクジの総数")
    slugs: list[DetectedSlug]
    overall_comment: str = Field(description="背景との溶け込み具合に関する全体評価")


def evaluate_camouflage(image_path: str):
    client = genai.Client()
    img = Image.open(image_path)

    prompt = """
    これは開発中の3Dゲーム（一人称視点）のスクリーンショットです。
    画面内のどこかに、2本足で立つデフォルメされたナメクジのキャラクターが1体または複数体隠れています。
    
    初見のプレイヤーの視点として、パッと見てナメクジがどこにいるかを探し出し、
    それぞれの座標（box_2d）、見つけやすさ（1〜10）、特に「黒い輪郭線（アウトライン）のせいで背景から浮いて見えていないか」を厳密に評価してください。
    まったく見つからない場合は detected_count を 0 にしてください。
    """

    response = client.models.generate_content(
        model="gemini-2.5-flash",
        contents=[img, prompt],
        config=types.GenerateContentConfig(
            response_mime_type="application/json",
            response_schema=CamouflageReport,
            temperature=0.2,  # 判定のブレを抑えるため低めに設定
        ),
    )

    print(f"=== 判定結果: {image_path} ===")
    print(json.dumps(json.loads(response.text), indent=2, ensure_ascii=False))


if __name__ == "__main__":
    # Screenshots/CamoTest/ フォルダ内の画像をすべて自動判定する例
    for path in sorted(glob.glob("Screenshots/CamoTest/*.png")):
        evaluate_camouflage(path)