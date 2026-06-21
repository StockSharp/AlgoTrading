# Estratégia de Canal BrakeExp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no indicador **BrakeExp**, que constrói um canal exponencial em torno dos movimentos de preço. O indicador alterna entre regimes comprado e vendido e gera sinais de compra ou venda quando o preço cruza as bordas dinâmicas do canal.

## Como funciona

- O indicador mantém uma curva exponencial que segue o preço.
- Quando a curva está abaixo do preço (tendência de alta), a estratégia procura sinais de compra.
- Quando a curva está acima do preço (tendência de baixa), a estratégia procura sinais de venda.
- Um cruzamento de um lado para o outro produz um sinal de entrada na nova direção e fecha a posição oposta.

## Parâmetros

- `Candle Type` – período temporal dos candles processados.
- `Volume` – volume da ordem para entradas de mercado.
- `A`, `B` – parâmetros que definem a forma da curva BrakeExp.
- `Buy Open` / `Sell Open` – permissão para abrir posições compradas ou vendidas.
- `Buy Close` / `Sell Close` – permissão para fechar posições vendidas ou compradas.

## Notas

Esta implementação concentra-se na lógica principal do indicador BrakeExp e não inclui gerenciamento de stop-loss ou take-profit. Controles de risco adicionais podem ser adicionados se necessário.
