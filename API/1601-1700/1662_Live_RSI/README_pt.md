# Estratégia RSI ao Vivo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza múltiplos cálculos de RSI (close, weighted, typical, median, open) e Parabolic SAR para detectar reversões de tendência. Entra comprado quando os valores de RSI se alinham em ordem de alta e o preço está acima do SAR; entra vendido quando o alinhamento é de baixa e o preço está abaixo do SAR. O valor do SAR atua como trailing stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando a sequência RSI é de alta e o preço está acima do SAR.
  - Vendido quando a sequência RSI é de baixa e o preço está abaixo do SAR.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal de tendência oposta ou trailing stop SAR.
- **Stops**: Stop-loss fixo opcional mais trailing stop baseado em SAR.
- **Valores padrão**:
  - `RSI Period` = 30
  - `SAR Step` = 0.08
  - `Stop Loss` = 40
  - `Check Hour` = false
  - `Start Hour` = 17
  - `End Hour` = 1
  - `Candle Type` = 1 hora
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: RSI, Parabolic SAR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Opcional (filtro de tempo)
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
