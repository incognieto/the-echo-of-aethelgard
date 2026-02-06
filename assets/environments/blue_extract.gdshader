shader_type spatial;
render_mode blend_mix, depth_draw_always;

uniform vec4 water_color : source_color = vec4(0.0, 0.5, 0.8, 0.4);
uniform float roughness : hint_range(0.0, 1.0) = 0.1;

void vertex() {
    float wave = sin(TIME * 0.2 + VERTEX.x * 10.0) * 0.01;
    VERTEX.y += wave;
}

void fragment() {
    ALBEDO = water_color.rgb;
    ALPHA = water_color.a; // PASTIKAN ALPHA DI INSPECTOR GAK NOL
    ROUGHNESS = roughness;
    SPECULAR = 0.8;
}