/* wwwroot/js/upload-media-script.js */

let mediaRecorder;
let recordedChunks = [];
let recordingType = null;

const startBtn = document.getElementById('startRecording');
const audioStartBtn = document.getElementById('startAudioRecording');
const stopBtn = document.getElementById('stopRecording');
const preview = document.getElementById('preview');
const audioPreview = document.getElementById('audioPreview');
const recordedFileInput = document.getElementById('recordedFile');
const uploadRecordedBtn = document.getElementById('uploadRecorded');
const loadingIndicator = document.getElementById('loading');
const themeToggle = document.getElementById('themeToggle');

// 1. منطق تبديل المظهر
function applyTheme(theme) {
    document.body.dataset.theme = theme;
    themeToggle.textContent = theme === 'dark' ? '☀️' : '🌙';
    localStorage.setItem('theme', theme);
}

themeToggle.addEventListener('click', () => {
    const currentTheme = document.body.dataset.theme;
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    applyTheme(newTheme);
});

// تحميل المظهر المفضل عند بدء التشغيل
document.addEventListener('DOMContentLoaded', () => {
    const savedTheme = localStorage.getItem('theme') || 'light';
    applyTheme(savedTheme);

    // تهيئة Tooltip من Bootstrap (يجب التأكد من تحميل Bootstrap JS)
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// 2. منطق التسجيل (WebRTC)
function startRecording(mediaConstraints, type) {
    // إخفاء الـ previews القديمة
    preview.style.display = 'none';
    audioPreview.style.display = 'none';

    navigator.mediaDevices.getUserMedia(mediaConstraints)
        .then(stream => {
            recordingType = type;
            if (type === 'video') {
                preview.srcObject = stream;
                preview.style.display = 'block';
                preview.muted = true; // لمنع صدى الصوت أثناء التسجيل
            } else {
                audioPreview.srcObject = stream;
                audioPreview.style.display = 'block';
            }

            recordedChunks = [];
            mediaRecorder = new MediaRecorder(stream);

            mediaRecorder.ondataavailable = e => {
                if (e.data.size > 0) recordedChunks.push(e.data);
            };

            mediaRecorder.onstop = () => {
                // إيقاف مسار الوسائط
                stream.getTracks().forEach(track => track.stop());

                const mimeType = type === 'video' ? "video/webm" : "audio/webm";
                const blob = new Blob(recordedChunks, { type: mimeType });
                const url = URL.createObjectURL(blob);

                if (type === 'video') {
                    preview.src = url;
                    preview.srcObject = null;
                    preview.muted = false; // تشغيل الصوت بعد التسجيل
                } else {
                    audioPreview.src = url;
                    audioPreview.srcObject = null;
                }

                // تجهيز الملف للرفع في النموذج المخفي
                const fileName = type === 'video' ? "recorded.webm" : "recorded_audio.webm";
                const file = new File([blob], fileName, { type: blob.type });

                // تحديث حقل الملف المخفي
                const dt = new DataTransfer();
                dt.items.add(file);
                recordedFileInput.files = dt.files;

                uploadRecordedBtn.disabled = false;
            };

            mediaRecorder.start();

            startBtn.disabled = true;
            audioStartBtn.disabled = true;
            stopBtn.disabled = false;
            uploadRecordedBtn.disabled = true; // تعطيل زر الرفع حتى يتم الإيقاف
        })
        .catch(err => {
            alert("تعذر الوصول إلى الميكروفون أو الكاميرا: " + err.message);
        });
}

// ربط الأزرار بالدوال
startBtn.onclick = () => startRecording({ audio: true, video: true }, 'video');
audioStartBtn.onclick = () => startRecording({ audio: true }, 'audio');

stopBtn.onclick = () => {
    if (mediaRecorder && mediaRecorder.state !== 'inactive') mediaRecorder.stop();
    startBtn.disabled = false;
    audioStartBtn.disabled = false;
    stopBtn.disabled = true;
};

// 3. منطق عرض شريط التحميل عند إرسال أي نموذج
document.querySelectorAll("form").forEach(f => {
    f.addEventListener("submit", () => {
        loadingIndicator.style.display = "block";
    });
});