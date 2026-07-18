# Estratégia de Rompimento TMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia explora rompimentos relativos a uma Média Móvel Triangular (TMA). Ela monitora uma série de candles configurável e compara o fechamento do candle anterior com o valor TMA mais ou menos offsets definidos pelo usuário. Uma posição comprada é aberta quando o fechamento anterior está acima de `TMA + UpLevel`, e uma posição vendida é aberta quando está abaixo de `TMA - DownLevel`. Sinais opostos revertem a posição.

## Parâmetros

- **TMA Length** – período utilizado para calcular a Média Móvel Triangular.
- **Upper Level** – offset de preço adicionado ao TMA para detectar sinais de compra.
- **Lower Level** – offset de preço subtraído do TMA para detectar sinais de venda.
- **Candle Type** – período dos candles utilizados pela estratégia.

## Como Funciona

1. Subscreve a série de candles selecionada.
2. Vincula um indicador de Média Móvel Triangular aos candles.
3. Em cada candle finalizado:
   - Armazena os valores anteriores de TMA e preço de fechamento.
   - Verifica se o fechamento anterior ultrapassou o nível superior ou inferior.
   - Envia ordens de mercado para abrir ou reverter posições conforme necessário.
4. Plota candles, linha do indicador e operações próprias para análise visual.

## Observações

A estratégia utiliza ordens de mercado sem gerenciamento de stop-loss ou take-profit. Destina-se a fins educacionais e deve ser expandida com controles de risco adequados antes da operação em tempo real.
