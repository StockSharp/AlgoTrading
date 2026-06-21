# Estratégia de Arrays de Correlação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula uma matriz de correlação deslizante para até seis ativos. Registra os níveis de correlação usando limiares configuráveis para ajudar a avaliar as relações entre ativos. A estratégia é apenas de análise e não executa operações.

## Detalhes
- **Critérios de entrada**: Nenhum (somente análise)
- **Comprado/Vendido**: Nenhum
- **Critérios de saída**: Nenhum
- **Stops**: Nenhum
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 100
  - `PositiveWeak` = 0.3
  - `PositiveMedium` = 0.5
  - `PositiveStrong` = 0.7
  - `NegativeWeak` = -0.3
  - `NegativeMedium` = -0.5
  - `NegativeStrong` = -0.7
- **Filtros**:
  - Categoria: Análise estatística
  - Direção: Nenhum
  - Indicadores: Correlação
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
