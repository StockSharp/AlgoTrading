# Estratégia de Spread de Alto Rendimento com Filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera com base no High Yield Spread ou no índice VIX. Uma posição é aberta quando o spread escolhido cruza um limiar e um filtro de preço opcional confirma. O filtro de preço exige que o fechamento esteja acima de uma média móvel simples para posições compradas, ou abaixo para vendidas. As posições são fechadas após um número fixo de barras.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Spread > limiar e fechamento > SMA (se habilitado).
  - **Vendido**: Spread < limiar e fechamento < SMA (se habilitado).
- **Comprado/Vendido**: Ambos, selecionado via parâmetro.
- **Critérios de saída**:
  - Fechar posição após as barras do período de manutenção.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Threshold` = 5
  - `HoldingPeriod` = 5
  - `SmaLength` = 50
- **Filtros**:
  - Categoria: Macro
  - Direção: Ambos
  - Indicadores: High Yield Spread/VIX, SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: 1d (padrão)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
