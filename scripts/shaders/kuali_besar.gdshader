shader_type spatial;

uniform vec3 liquid_color : source_color = vec3(0.0, 0.8, 0.1);
uniform sampler2D noise_img; // Isi pake FastNoiseLite

void fragment() {
    // Bikin dua UV yang gerak ke arah berbeda buat efek tabrakan air
    vec2 uv1 = UV + vec2(TIME * 0.03, TIME * 0.02);
    vec2 uv2 = UV + vec2(TIME * -0.02, TIME * 0.04);
    
    float n1 = texture(noise_img, uv1).r;
    float n2 = texture(noise_img, uv2).r;
    
    // Campurin kedua noise biar teksturnya "ngacak" dan fluid
    float final_noise = (n1 + n2) * 0.5;
    
    // Bikin efek garis-garis terang (selaput cairan) pake smoothstep
    float rim = smoothstep(0.4, 0.6, final_noise);
    
    ALBEDO = liquid_color + (rim * 0.3); // Campur warna dasar sama highlight terang
    EMISSION = liquid_color * rim * 2.0; // Biar bagian highlight-nya glow
    NORMAL = vec3(0, 1, 0) + (final_noise * 0.1); // Kasih sedikit tekstur permukaan
}