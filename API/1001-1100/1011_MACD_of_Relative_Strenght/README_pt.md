# Estratégia MACD de Força Relativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula a força relativa dividindo o preço de fechamento pela máxima mais alta em um período especificado e aplica o indicador MACD a essa razão. Uma posição comprada é aberta quando o histograma MACD é positivo e fechada quando se torna negativo. Um stop-loss percentual protege a operação.

## Detalhes
- **Entrada**: Histograma > 0.
- **Saída**: Histograma < 0 ou stop-loss.
- **Tipo**: Somente comprado.
- **Indicadores**: Highest, MACD.
