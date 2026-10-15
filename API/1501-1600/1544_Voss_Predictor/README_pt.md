# Estratégia Voss Predictor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o filtro preditivo Voss de John Ehlers com um filtro passa-banda para antecipar o movimento do preço. Uma posição comprada é aberta quando o filtro preditivo sobe acima da saída do passa-banda, enquanto uma posição vendida é aberta quando ele cai abaixo.

## Detalhes

- **Entrada**: O filtro preditivo Voss cruza acima do filtro passa-banda.
- **Saída**: O filtro preditivo Voss cruza abaixo do filtro passa-banda.
- **Tipo**: Seguidor de tendência.
- **Stops**: Nenhum.
