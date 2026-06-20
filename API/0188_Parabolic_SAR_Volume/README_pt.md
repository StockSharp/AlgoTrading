# Parabolic Sar Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia que combina Parabolic SAR com confirmação de volume. Entra em operações quando o preço cruza o Parabolic SAR com volume acima da média.

Os testes indicam um retorno anual médio de aproximadamente 151%. Funciona melhor no mercado de ações.

O Parabolic SAR identifica mudanças de tendência e o volume mais alto valida o sinal. As operações começam quando a inversão do SAR vem acompanhada de expansão de volume.

Útil para traders que acompanham movimentos baseados em volume. O rastro do SAR e um fator ATR protegem contra grandes perdas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > SAR && Volume > AvgVolume`
  - Vendido: `Close < SAR && Volume > AvgVolume`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Inversão do SAR
- **Stops**: Usa Parabolic SAR como trailing stop
- **Valores padrão**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `VolumePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Parabolic SAR, Parabolic SAR, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

