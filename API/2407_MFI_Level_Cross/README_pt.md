# Estratégia de Cruzamento de Nível MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador Money Flow Index (MFI) para identificar condições de sobrecompra e sobrevenda. Quando o MFI cruza níveis de limite predefinidos, a estratégia entra ou reverte posições. Pode operar na direção do cruzamento ou na direção oposta, dependendo do modo de tendência selecionado.

A configuração padrão monitora velas de quatro horas e avalia o MFI de 14 períodos. A estratégia abre uma posição comprada quando o MFI cai abaixo do limite inferior e uma posição vendida quando sobe acima do limite superior. Quando definido para o modo "Against", a lógica de entrada é invertida para negociar contra a direção do indicador.

O gerenciamento de risco é feito através de parâmetros integrados de stop-loss e take-profit expressos como porcentagens do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - **Trend Mode: Direct**:
    - **Comprado**: MFI anterior > nível baixo e MFI atual ≤ nível baixo.
    - **Vendido**: MFI anterior < nível alto e MFI atual ≥ nível alto.
  - **Trend Mode: Against**:
    - **Comprado**: MFI anterior < nível alto e MFI atual ≥ nível alto.
    - **Vendido**: MFI anterior > nível baixo e MFI atual ≤ nível baixo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: A posição é revertida quando o sinal oposto aparece ou fechada pelo módulo de proteção.
- **Stops**: Stop-loss e take-profit expressos em percentual do preço de entrada.
- **Valores padrão**:
  - `Candle Type` = velas de 4 horas.
  - `MFI Period` = 14.
  - `Low Level` = 40.
  - `High Level` = 60.
  - `Stop Loss %` = 1.
  - `Take Profit %` = 2.
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Configurável
  - Indicadores: Money Flow Index
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Notas

Esta implementação depende da API de alto nível do StockSharp. Assina dados de velas, vincula o indicador MFI diretamente e executa ordens de mercado quando as condições de cruzamento são atendidas. A proteção de posição é inicializada uma vez na inicialização para gerenciar o risco automaticamente.
