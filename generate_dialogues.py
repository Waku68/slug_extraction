import json
import os
from google import genai
from google.genai import types
from pydantic import BaseModel, Field


# 1. Unity側でそのままJsonUtilityやNewtonsoft.Jsonで読めるスキーマ定義
class MerchantLine(BaseModel):
    item_id: str = Field(description="植物のID（例: amekabu, ugotake, tsuyuri）")
    market_state: str = Field(description="相場状態（high: 品薄高騰, normal: ふつう, low: 供給過多）")
    season: str = Field(description="季節（heavy_rain: 大雨季, dry: 乾季, rain: 雨季, heavy_dry: 大乾季）")
    trust_level: int = Field(description="なじみ度（0: 初対面〜浅い, 1: 顔なじみ, 2: 深い信頼）")
    dialogue: str = Field(description="行商人のひとこと（40〜70文字程度。数字は出さず、空気感と相場のヒントを伝える）")


class MerchantDialogueList(BaseModel):
    lines: list[MerchantLine]


def generate_merchant_dialogues():
    client = genai.Client()  # 環境変数 GEMINI_API_KEY を自動で読み込む

    prompt = """
    あなたは「ナメクジ脱出PvP」に登場する拠点のNPC「花と草の行商人（カタツムリ）」です。
    以下の世界観とルールに従い、植物の買取相場をプレイヤーに匂わせる「ひとこと」を生成してください。

    【世界観・口調ルール】
    - 話し手は落ち着いた老練なカタツムリの行商人（一人称は「わたし」または省略、語尾は「〜だねえ」「〜かい」「〜だよ」など少し乾いた温かみのある職人・商人言葉）。
    - この世界では数字やグラフで相場を見せない。「近ごろ○○が入らなくてねえ」「今日はみんなそれを持ってくるよ」のように、品薄かダブついているかを言葉で伝える。
    - 供給過多（low）のときは、「うちの値じゃ隣の問屋と変わらないよ（問屋に売った方がいい）」というニュアンスをにじませる。
    - なじみ度（trust_level）が 0 のときは少し他人行儀、2 のときは「あんたにだけ言うけどね」と親密な態度にする。

    【生成対象】
    今回はテストとして以下の2種類の植物について、
    相場状態（high, normal, low）× 季節（heavy_rain, heavy_dry）× なじみ度（0, 2）の全組み合わせ（計24パターン）を生成してください。
    1. amekabu（雨カブ：雨の森に生える定番の作物。かじると湿り気が戻る）
    2. tsuyuri（ツユリ：大雨が止んだ晴れ間の1時間だけ咲く珍しい花）
    """

    response = client.models.generate_content(
        model="gemini-3.8-flash",
        contents=prompt,
        config=types.GenerateContentConfig(
            response_mime_type="application/json",
            response_schema=MerchantDialogueList,
            temperature=0.7,
        ),
    )

    # UnityのResourcesやStreamingAssetsにそのまま入れられるJSONとして保存
    with open("merchant_dialogues.json", "w", encoding="utf-8") as f:
        f.write(response.text)
    print("生成完了: merchant_dialogues.json に保存しました。")


if __name__ == "__main__":
    generate_merchant_dialogues()