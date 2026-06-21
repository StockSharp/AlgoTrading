# Estratégia Pavan CPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera comprado quando o preço cruza acima do Intervalo de Pivô Central superior do dia após ter fechado previamente abaixo dele. O stop é colocado no nível do pivô e o take profit a uma distância fixa.

## Detalhes

- **Critérios de entrada**: Fechamento anterior abaixo do CPR superior e fechamento atual acima dele.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Take profit ou stop no pivô.
- **Stops**: Sim.
- **Valores padrão**:
  - `TakeProfitTarget` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: Pivot
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
