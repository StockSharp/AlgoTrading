# Canal de Regressão Linear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia recuos quando o preço se move fora de um canal de regressão linear e reverte em direção à média.

## Detalhes

- **Dados**: Velas de preço.
- **Entrada**: Comprar quando o fechamento cai abaixo da banda inferior em uma tendência de alta; vender quando o fechamento sobe acima da banda superior em uma tendência de baixa.
- **Saída**: Fechar quando o preço retorna à linha de regressão.
- **Instrumentos**: Qualquer instrumento.
- **Risco**: Os limites do canal atuam como limites dinâmicos.
