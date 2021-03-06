#version 120

uniform sampler2D texture;
uniform float texture_inverse;
uniform int blur_radius;
uniform vec2 blur_direction;

void main(void){
    vec4 sum = texture2D(texture, gl_TexCoord[0].xy);

    for (int i = 0; i < blur_radius; ++i){
        sum += texture2D(texture, gl_TexCoord[0].xy + (i * texture_inverse) * blur_direction);
        sum += texture2D(texture, gl_TexCoord[0].xy - (i * texture_inverse) * blur_direction);
    }
    vec4 pixel = vec4(sum / (blur_radius * 2 + 1));
    pixel.rgb *= pixel.a; //to negate the sf::BlendMode effect
    gl_FragColor = pixel;
}
