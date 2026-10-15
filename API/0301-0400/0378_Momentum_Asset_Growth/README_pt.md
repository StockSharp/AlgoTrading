# Estratégia de Momentum e Crescimento de Ativos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de fator híbrida combina o momentum de preços com o efeito de crescimento de ativos. Empresas que expandem rapidamente seus balanços e simultaneamente mostram preços com tendências fortes frequentemente são recompensadas pelo mercado. A abordagem primeiro filtra o universo para selecionar empresas no decil superior de crescimento de ativos.

As ações elegíveis são então classificadas por momentum de doze meses, excluindo o mês mais recente para evitar reversões de curto prazo. O quintil superior de momentum é comprado enquanto o quintil inferior é vendido a descoberto. O rebalanceamento ocorre no primeiro dia útil de cada mês, exceto em janeiro, quando a estratégia fica inativa. Nenhum stop é aplicado entre as revisões.

Testes retrospectivos em ações de mercados desenvolvidos indicam que a combinação de expansão de ativos e momentum entrega retornos robustos com rotatividade moderada.

## Detalhes

- **Critérios de entrada**: Mensal; selecionar o decil superior de crescimento de ativos e depois classificar por
  momentum; comprado no quintil superior, vendido no quintil inferior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Próximo rebalanceamento mensal (janeiro ignorado)
- **Stops**: Não
- **Valores padrão**:
  - `MomLook` = 252
  - `SkipMonths` = 1
  - `AssetDecile` = 10
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Momentum, Fundamentos
  - Direção: Ambos
  - Indicadores: Momentum de preço, crescimento de ativos
  - Stops: Não
  - Complexidade: Avançado
  - Período: Médio prazo
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
