# Estratégia Exp Fishing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em uma posição quando o fechamento do candle concluído difere de sua abertura em pelo menos **Price Step**. Se a diferença for positiva, compra; se for negativa, vende.

Após abrir uma posição, cada movimento adicional de **Price Step** a favor da operação dispara uma ordem de mercado adicional na mesma direção até **Max Orders**. Stop-loss e take-profit de proteção são aplicados para cada posição usando distâncias absolutas de preço.

## Parâmetros

- **Price Step** – movimento mínimo de preço (em unidades absolutas) necessário para abrir ou adicionar a uma posição.  
- **Max Orders** – número máximo de ordens de mercado permitidas em uma direção.  
- **Stop Loss** – distância do preço de entrada onde um stop de proteção é colocado.  
- **Take Profit** – distância do preço de entrada onde o alvo de lucro é colocado.  
- **Candle Type** – período do candle usado para cálculos (padrão 1 minuto).

## Lógica de Negociação

1. Aguardar um candle concluído.
2. Se nenhuma posição estiver aberta:
   - Comprar se `Close - Open >= Price Step`.
   - Vender se `Open - Close >= Price Step`.
3. Quando uma posição existe:
   - Se o preço avançar `Price Step` desde a última entrada, adicionar outra ordem na mesma direção.
   - Parar de adicionar ordens quando o número atingir **Max Orders**.
4. Stop-loss e take-profit são gerenciados automaticamente para cada ordem.

A estratégia é adaptada do especialista MQL5 "Exp Fishing" e demonstra uma abordagem simples de seguimento de tendência estilo grade.
