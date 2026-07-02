# Esquema de Estratégia Avançada com Múltiplos Prazos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este arquivo ilustra um esquema de estratégia complexo que utiliza velas de diferentes prazos, projetado especificamente para a plataforma Designer da StockSharp. Este exemplo emprega condições variadas em múltiplos ramos para executar negociações com base em dados históricos de preços.

## Detalhes da Estratégia

O esquema é dividido em dois ramos principais, cada um utilizando velas de cinco minutos comparadas com extremos históricos de preços para tomar decisões de negociação:

### Primeiro Ramo — Extremos Históricos
- **Condição de Compra**: A estratégia inicia uma ordem de compra se o preço de fechamento de uma vela de cinco minutos for maior que o preço mais alto dos últimos 20 dias.
- **Condição de Venda**: Uma ordem de venda é executada se o preço de fechamento de uma vela de cinco minutos for menor que o preço mais baixo dos últimos 10 dias.

### Segundo Ramo — Condições Inversas
- **Condição de Venda**: Executa uma ordem de venda se o preço de fechamento de uma vela de cinco minutos for menor que o preço mais baixo dos últimos 20 dias.
- **Condição de Compra**: Inicia uma compra se o preço de fechamento de uma vela de cinco minutos for maior que o preço mais alto dos últimos 10 dias.

## Recursos e Alterações Específicos da Versão
- **Aparência do Bloco de Bandeira**: No Designer versão 5, a aparência do bloco de bandeira foi atualizada.
- **Adaptações da Estratégia**: Também na versão 5, a estratégia foi modificada para incluir dois blocos tanto para sinais de venda quanto de compra. Esse ajuste se deve a uma mudança na forma como os sinais acionam as ações na versão mais recente do Designer.

Este esquema fornece uma estrutura para implementar e testar estratégias que reagem a movimentos significativos de preços, comparando as ações de preço de curto prazo com os registros de preços de longo prazo. A abordagem de múltiplos ramos permite aos traders experimentar diferentes respostas estratégicas com base nos mesmos dados subjacentes.
