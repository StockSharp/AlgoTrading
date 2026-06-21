# Estratégia de Arco-Íris de Médias Móveis (Stormer)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia plota um arco-íris de doze médias móveis. As operações são abertas quando a tendência é confirmada e o preço toca uma das médias.

Uma posição comprada abre quando o preço faz uma nova máxima, todas as médias centrais inclinam para cima e a vela fecha acima da média de todas as médias. Uma posição vendida abre quando ocorrem as condições opostas.

O stop loss é definido na média móvel tocada anteriormente. O take profit é calculado como um múltiplo da distância entre o preço de entrada e o stop loss.

## Detalhes

- **Indicadores**: 12 médias móveis de tipo configurável.
- **Comprado**: Tendência de alta, nova máxima e preço de toque anterior.
- **Vendido**: Tendência de baixa, nova mínima e preço de toque anterior.
- **Saídas**: Stop loss na média tocada, alvo = entrada ± distância * fator. Saída de reversão opcional quando a tendência mostra sinais de reversão.
- **Parâmetros**: tipo de média móvel, comprimentos, fator alvo, opções de reversão.
- **Período**: Qualquer.
