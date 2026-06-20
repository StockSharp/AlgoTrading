# Estratégia do Primeiro Dia do Mês
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Muitos mercados exibem um viés de alta no primeiro dia de negociação do mês à medida que novo capital flui para os fundos.
Os traders tentam antecipar esse efeito comprando no fechamento do mês anterior ou no início da sessão.

Os testes indicam um retorno anual médio de aproximadamente 97%. Funciona melhor no mercado de criptomoedas.

A estratégia entra comprado no início do mês e sai antes do início do segundo dia, capturando o típico influxo de compras.

Um stop pequeno protege contra surpresas de baixa caso a força esperada não apareça.

## Detalhes

- **Critérios de entrada**: gatilhos de efeito calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

