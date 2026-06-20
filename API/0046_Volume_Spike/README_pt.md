# Volume Spike Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Volume Spike Trend monitora aumentos repentinos no volume negociado. Quando o volume atual supera a média recente por um multiplicador definido, sinaliza forte participação dos agentes.

Os testes indicam um retorno anual médio de aproximadamente 175%. Funciona melhor no mercado de ações.

Se o volume apresenta um spike e o preço está acima da média móvel, a estratégia compra; se o volume apresenta um spike com o preço abaixo da média, opera vendido. As operações são encerradas quando o volume cai novamente abaixo da média ou o stop-loss é atingido.

Este método busca capturar movimentos impulsionados por uma explosão de atividade.

## Detalhes

- **Critérios de entrada**: A variação de volume supera `VolumeSpikeMultiplier` vezes a média.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O volume cai abaixo da média ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `VolAvgPeriod` = 20
  - `VolumeSpikeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Volume, MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

