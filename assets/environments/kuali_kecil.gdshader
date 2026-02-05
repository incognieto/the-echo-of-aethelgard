shader_type spatial;

uniform vec3 color_main : source_color = vec3(0.0, 0.3, 0.8); // Biru
uniform vec3 color_secondary : source_color = vec3(0.4, 1.0, 0.4); // Hijau Muda
uniform sampler2D noise_img;

void fragment() {
    // 1. Efek Swirl (Pusaran)
    // Hitung jarak dari tengah (UV 0.5)
    vec2 uv = UV - 0.5;
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);
    
    // Putar UV berdasarkan waktu dan jarak dari tengah
    // Semakin jauh dari tengah, putaran semakin lambat (efek vortex)
    float swirl = angle + (TIME * 1.5) + (dist * 10.0);
    vec2 swirl_uv = vec2(cos(swirl), sin(swirl)) * dist + 0.5;
    
    // 2. Ambil Noise berdasarkan UV yang sudah diputar
    float n = texture(noise_img, swirl_uv + (TIME * 0.05)).r;
    
    // 3. Mixing Warna (Swirl Pattern)
    // Gunakan smoothstep untuk membuat batas warna biru dan hijau yang dinamis
    float mixer = smoothstep(0.3, 0.7, n);
    vec3 final_color = mix(color_main, color_secondary, mixer);
    
    ALBEDO = final_color;
    
    // 4. Glow & Detail
    // Bikin bagian hijau muda lebih nyala (glow) daripada birunya
    EMISSION = final_color * mixer * 2.5;
    ROUGHNESS = 0.2;
    SPECULAR = 0.5;
}