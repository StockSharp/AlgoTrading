# Sistema de Momentum de Gaps (Estratégia)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa o sistema de momentum de gaps de Perry Kaufman. A estratégia compara os gaps acumulados de alta e de baixa e opera quando o sinal sobe ou cai.

## Detalhes
- **Critérios de entrada**: Sinal em alta -> comprar, sinal em baixa -> vender ou reverter.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Period` = 40
  - `SignalPeriod` = 20
  - `LongOnly` = true
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos ou somente comprado
  - Indicadores: Gap momentum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
