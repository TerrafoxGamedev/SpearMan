shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(1.0, 0.9, 0.3, 1.0);  // Жёлтый
uniform float outline_size : hint_range(0.0, 16.0) = 2.0;              // Толщина в пикселях
uniform vec2 texture_size = vec2(64.0, 64.0);                          // Размер текстуры в пикселях

void fragment() {
    vec2 texel = 1.0 / texture_size;

    // Сэмплим альфу в 8 направлениях вокруг текущего пикселя
    float a = 0.0;
    a = max(a, texture(TEXTURE, UV + vec2( outline_size, 0.0) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2(-outline_size, 0.0) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2(0.0,  outline_size) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2(0.0, -outline_size) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2( outline_size,  outline_size) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2(-outline_size,  outline_size) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2( outline_size, -outline_size) * texel).a);
    a = max(a, texture(TEXTURE, UV + vec2(-outline_size, -outline_size) * texel).a);

    // Альфа самого пикселя
    float self_a = texture(TEXTURE, UV).a;

    // Если рядом есть непрозрачный пиксель, а сам пиксель прозрачный — это обводка
    if (a > 0.5 && self_a < 0.5) {
        COLOR = outline_color;
    } else {
        discard;   // Иначе — не рисуем ничего
    }
}