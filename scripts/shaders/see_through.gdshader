shader_type spatial;

// render_mode ini wajib agar tidak ada sekat antar tile GridMap
render_mode depth_draw_always, cull_disabled, diffuse_burley;

uniform sampler2D albedo_texture : source_color;
global uniform vec3 player_pos;

// ===== KONTROL VISUAL (Lingkaran) =====
uniform float circle_radius : hint_range(0.0, 1.0) = 0.15; 
uniform float smoothness    : hint_range(0.0, 1.0) = 0.08;
uniform float min_opacity   : hint_range(0.0, 1.0) = 0.15; // Tekstur bintik transparan

// ===== FILTER ARAH (Agar Samping & Atas Solid) =====
uniform float x_squeeze     : hint_range(1.0, 4.0) = 1.5;  // Makin besar makin sempit area bolongnya
uniform float y_offset      : hint_range(-0.5, 0.5) = 0.1; // Geser lubang ke bawah (kaki player)
uniform float z_margin      : hint_range(-1.0, 1.0) = 0.2; // Batas depan/belakang

varying vec3 v_world_pos;
varying vec4 v_player_clip;

// Dithering Global (Kunci agar antar kotak mulus 100%)
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
    // MENGUNCI POSISI DUNIA PIXEL: 
    // Ini yang membuat transparan nyambung antar tile, bukan satu per satu tile.
    v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    
    // Proyeksi player ke layar
    v_player_clip = PROJECTION_MATRIX * (VIEW_MATRIX * vec4(player_pos, 1.0));
}

void fragment() {
    vec4 tex = texture(albedo_texture, UV);
    ALBEDO = tex.rgb;

    // 1. Hitung Posisi Layar Player
    vec2 p_uv = (v_player_clip.xy / v_player_clip.w) * 0.5 + 0.5;
    p_uv.y = 1.0 - p_uv.y;
    vec2 center = p_uv + vec2(0.0, -0.05);

    // 2. Hitung Jarak Lingkaran di Layar (Perspective Friendly)
    vec2 diff = SCREEN_UV - center;
    diff.x *= x_squeeze; // Biar bentuknya agak oval vertikal, cocok buat top-down
    float dist = length(diff);
    float circle_mask = smoothstep(circle_radius, circle_radius + smoothness, dist);

    // 3. FILTER SUMBU Z DUNIA (KHUSUS DINDING BAWAH)
    // Logika: Hanya dinding yang posisi Z-nya lebih besar (arah kamera) dari player.
    float is_in_front = smoothstep(z_margin - 0.1, z_margin + 0.1, v_world_pos.z - player_pos.z);
    
    // 4. GABUNGKAN
    // Jika dinding di belakang (atas) atau samping jauh, is_in_front akan 0, maka mask jadi 1.0 (Opaque)
    float final_factor = mix(1.0, circle_mask, is_in_front);
    float final_opacity = max(final_factor, min_opacity);

    // 5. Eksekusi Dither
    if (dither_check(FRAGCOORD.xy, final_opacity)) {
        discard;
    }
}