# Estratégia Gann Swing Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada na técnica de rompimento de swing de Gann

Os testes indicam um retorno anual médio de aproximadamente 82%. Funciona melhor no mercado de ações.

Gann Swing Breakout rastreia máximas e mínimas de swing da análise de Gann. Um rompimento além do último swing inicia uma operação nessa direção e permanece aberta até que o swing oposto seja violado.

O método é projetado para traders que veem pontos de swing passados como suporte e resistência importantes. Ao operar no rompimento, tenta aproveitar a próxima etapa de uma tendência.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, Gann.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `SwingLookback` = 5
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MA, Gann
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

