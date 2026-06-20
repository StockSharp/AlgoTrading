# Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada no indicador Williams %R

Os testes indicam um retorno anual médio de aproximadamente 88%. Funciona melhor no mercado de ações.

Williams %R identifica zonas de sobrecompra e sobrevenda. Quando o indicador sobe acima do limite superior sinaliza fraqueza potencial para posições vendidas; leituras abaixo do limite inferior sugerem posições compradas. As posições fecham quando %R se move em direção à zona neutra.

Como %R oscila rapidamente, a estratégia pode gerar muitos sinais em mercados voláteis. Alguns traders o combinam com outros filtros para reduzir o ruído.


## Detalhes

- **Critérios de entrada**: Sinais baseados em Williams.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `Period` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Williams
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

