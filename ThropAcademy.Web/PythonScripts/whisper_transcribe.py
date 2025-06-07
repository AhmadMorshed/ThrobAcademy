import sys
import whisper

import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')



if len(sys.argv) < 2:
    print("❌ لا يوجد مسار لملف صوتي.")
    sys.exit(1)

audio_path = sys.argv[1]

model = whisper.load_model("base")  

try:
    result = model.transcribe(audio_path)
    print(" ✅ ")
    print(result["text"])  
except Exception as e:
    print("❌ خطأ أثناء التحويل:", str(e))
