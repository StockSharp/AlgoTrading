# Estratégia Simple Bars
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Simple Bars replica o comportamento do expert MQL5 original `Exp_SimpleBars`. Utiliza o indicador *SimpleBars* para determinar a tendência atual comparando a última vela com as máximas e mínimas recentes. Quando o indicador detecta uma mudança de tendência, a estratégia executa uma operação na abertura da próxima barra.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: O sinal do indicador na barra anterior é *buy*.
  - **Vendido**: O sinal do indicador na barra anterior é *sell*.
- **Comprado/Vendido**: Ambas as direções são operadas.
- **Critérios de saída**
  - A posição é revertida quando surge um sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**
  - `Period` = 6 barras.
  - `UseClose` = `true` (o preço de fechamento é utilizado para comparação).
  - `CandleType` = velas de 6 horas.
- **Filtros**
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: Personalizado.
  - Stops: Não.
  - Complexidade: Moderado.
  - Período: Médio prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
