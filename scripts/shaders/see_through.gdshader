shader_type spatial;

group_uniforms Data_Tekstur;
uniform sampler2D tekstur_dinding : source_color;
group_uniforms;

// Menggabungkan semua fitur dalam satu render mode
render_mode depth_draw_always, cull_disabled, diffuse_toon, specular_toon;

// SLOT UNTUK IMAGE TEXTURE KAMU (Akan muncul di Inspector)

global uniform vec3 player_pos;

// ===== KONTROL VISUAL (Lingkaran) =====
uniform float circle_radius : hint_range(0.0, 1.0) = 0.15; 
uniform float smoothness    : hint_range(0.0, 1.0) = 0.08;
uniform float min_opacity   : hint_range(0.0, 1.0) = 0.15;

// ===== FILTER ARAH =====
uniform float x_squeeze     : hint_range(1.0, 4.0) = 1.5; 
uniform float y_offset      : hint_range(-0.5, 0.5) = -0.05;
uniform float z_margin      : hint_range(-1.0, 1.0) = 0.2;

// ===== KONTROL TOON (Bayangan) =====
uniform vec4 shadow_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float shadow_threshold : hint_range(0.0, 1.0) = 0.5;

varying vec3 v_world_pos;
varying vec4 v_player_clip;

bool dither_check(vec2 screen_pos, float opacity) {
    const mat4 dither_matrix = mat4(
        vec4(0.0625, 0.5625, 0.1875, 0.6875),
        vec4(0.8125, 0.3125, 0.9375, 0.4375),
        vec4(0.25,   0.75,   0.125,  0.625),
        vec4(1.0,    0.5,    0.875,  0.375)
    );
    int x = int(screen_pos.x) % 4;
    int y = int(screen_pos.y) % 4;
    return opacity < dither_matrix[x][y];
}

void vertex() {
    v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    v_player_clip = PROJECTION_MATRIX * (VIEW_MATRIX * vec4(player_pos, 1.0));
}

void fragment() {
    // DISINI KITA PANGGIL IMAGE TEXTURE KAMU
    vec4 tex = texture(tekstur_dinding, UV); // Ganti namanya di sini juga
    ALBEDO = tex.rgb; // Sekarang warna dinding diambil dari gambar kamu

    // 1. Hitung Posisi Layar Player
    vec2 p_uv = (v_player_clip.xy / v_player_clip.w) * 0.5 + 0.5;
    p_uv.y = 1.0 - p_uv.y;
    vec2 center = p_uv + vec2(0.0, y_offset);

    // 2. Efek Lingkaran
    vec2 diff = SCREEN_UV - center;
    diff.x *= x_squeeze; 
    float dist = length(diff);
    float circle_mask = smoothstep(circle_radius, circle_radius + smoothness, dist);

    // 3. Filter Sumbu Z
    float is_in_front = smoothstep(z_margin - 0.1, z_margin + 0.1, v_world_pos.z - player_pos.z);
    
    // 4. Gabungkan
    float opacity_target = mix(1.0, circle_mask, is_in_front);
    float final_opacity = max(opacity_target, min_opacity);

    // 5. DISCARD (Ini yang bikin nembus beneran ke player, bukan cuma nembus ke material dalem)
    if (dither_check(FRAGCOORD.xy, final_opacity)) {
        discard;
    }
}
