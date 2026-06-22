# Tendência Alexcud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Trend Alexcud procura movimentos direcionais fortes alinhando múltiplas médias móveis simples e o Accelerator Oscillator em três períodos de tempo. Foi convertida do expert MQL5 original "TREND_alexcud v_2".

O sistema observa três períodos de tempo (padrão 15 minutos, 1 hora, 4 horas). Em cada período calcula cinco médias móveis simples (períodos 5, 8, 13, 21, 34) e o Accelerator Oscillator. Um período é considerado de alta quando o preço de fechamento está acima de todas as médias móveis e o Accelerator é positivo. Um período é de baixa quando o preço de fechamento está abaixo de todas as médias móveis e o Accelerator é negativo.

Uma operação é aberta somente quando os três períodos concordam. Se estão simultaneamente em alta, a estratégia compra; uma leitura de baixa em comum aciona uma venda. A posição é revertida quando o sinal oposto aparece. As ordens de proteção são gerenciadas pelo sistema de risco integrado do StockSharp.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: Preço acima de todas as MAs e Accelerator > 0 em cada período.
  - **Vendido**: Preço abaixo de todas as MAs e Accelerator < 0 em cada período.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: A posição se reverte quando o sinal oposto se forma.
- **Stops**: Usa proteção integrada (sem valores padrão).
- **Valores padrão**:
  - Timeframe1 = 15m, Timeframe2 = 1h, Timeframe3 = 4h
  - Períodos de MA = 5, 8, 13, 21, 34
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Multi-timeframe
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
