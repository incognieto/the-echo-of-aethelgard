shader_type spatial;

uniform vec3 color_biru : source_color = vec3(0.0, 0.4, 1.0);
uniform vec3 color_hijau_muda : source_color = vec3(0.3, 1.0, 0.5);
uniform sampler2D noise_img;

void fragment() {
    // Ambil koordinat dari posisi X dan Z lokal (pusat pasti 0,0)
    vec2 pos = VERTEX.xz; 
    
    float dist = length(pos);
    float angle = atan(pos.y, pos.x);
    
    // Efek Pusaran (Swirl)
    // Naikin angka 15.0 kalau mau lilitannya lebih banyak
    float swirl = angle + (dist * 15.0) - (TIME * 2.0);
    
    // Mapping kembali ke UV buat baca noise
    vec2 swirled_uv = vec2(cos(swirl), sin(swirl)) * dist;
    
    // Ambil nilai noise (pake Frequency kecil di FastNoiseLite)
    float n = texture(noise_img, swirled_uv * 0.5 + 0.5).r;
    
    // Mixing Warna
    float mixer = smoothstep(0.2, 0.7, n);
    vec3 final_color = mix(color_biru, color_hijau_muda, mixer);
    
    ALBEDO = final_color;
    
    // EMISSION murni dari kode, jangan dari menu Inspector lagi
    // Ini biar hijaunya gak nimpa birunya secara keseluruhan
    EMISSION = final_color * (mixer * 2.5); 
}