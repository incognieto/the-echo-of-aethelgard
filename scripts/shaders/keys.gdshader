shader_type canvas_item;

uniform float target_index = 0.0; 
uniform float total_frames = 6.0;

void fragment() {
    vec2 uv = UV;
    
    // 1. Perkecil UV Y sesuai jumlah frame
    float frame_height = 1.0 / total_frames;
    
    // 2. Geser UV berdasarkan index yang dikirim C#
    // Kita gunakan fract() agar ketika index bertambah terus, UV-nya mengulang (loop)
    float scroll = target_index * frame_height;
    uv.y = uv.y * frame_height + scroll;
    
    // Karena kita sudah set 'Repeat' di Import, uv.y yang lebih dari 1.0 
    // akan otomatis mengambil gambar dari awal lagi.
    COLOR = texture(TEXTURE, uv);
}