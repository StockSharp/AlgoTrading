# Estratégia de Seguimento de Tendência ADX Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza ADX com movimento direcional e Parabolic SAR para seguir tendências. Posições compradas ocorrem quando ADX está acima de um limiar, +DI supera -DI e o preço está acima da linha SAR. Sinais vendidos usam a configuração oposta.

## Detalhes

- **Critérios de entrada**: ADX > limiar com cruzamento de DI e preço > SAR.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX, Parabolic SAR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
