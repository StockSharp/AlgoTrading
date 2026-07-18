# Estratégia de Pontos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do MQL5 "Exp_Dots". A estratégia opera reversões quando o indicador Dots muda de cor.
Vai comprado quando o indicador muda de azul para vermelho e vendido quando muda de vermelho para azul.

## Detalhes

- **Critérios de entrada**:
  - Comprado: A cor do indicador muda de azul para vermelho.
  - Vendido: A cor do indicador muda de vermelho para azul.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Length` = 10
  - `Filter` = 0m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Reversão de tendência
  - Direção: Ambos
  - Indicadores: Dots (NonLag Moving Average)
  - Stops: Não
  - Complexidade: Intermediário
  - Período: 4H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
